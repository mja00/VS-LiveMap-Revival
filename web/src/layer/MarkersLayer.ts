import * as L from 'leaflet';

import { Circle } from './marker/Circle';
import { Ellipse } from './marker/Ellipse';
import { Icon } from './marker/Icon';
import { Polygon } from './marker/Polygon';
import { Polyline } from './marker/Polyline';
import { Rectangle } from './marker/Rectangle';

import type { LiveMap } from '../LiveMap';
import type { Marker, MarkerJson, PopupOptions } from './marker/Marker';

interface Defaults {
	options: L.LayerOptions;
	popup: PopupOptions;
	tooltip: L.TooltipOptions;
}

interface LayerJson {
	label: string;
	interval: number;
	hidden: boolean;
	options: L.LayerOptions;
	defaults: Defaults;
	markers: MarkerJson[];
	css: string;
}

export class MarkersLayer extends L.LayerGroup {
	protected readonly _livemap: LiveMap;
	private readonly _url: string;

	// @ts-expect-error - typescript doesn't know about the markercluster plugin
	private _cluster?;
	private readonly _markers: Map<string, Marker> = new Map();

	private _id?: string;
	private _label?: string;
	private _interval?: number;
	private _defaults?: Defaults;
	private _json?: LayerJson;

	private _updating: boolean = false;

	constructor(livemap: LiveMap, url: string, interval?: number) {
		super([]);
		this._livemap = livemap;
		this._url = url;
		this._interval = interval;

		// initial update to get interval and label
		this.updateLayer();
	}

	get markers(): Map<string, Marker> {
		return this._markers;
	}

	get id(): string {
		return this._id!;
	}

	get label(): string {
		return this._label!;
	}

	set label(label: string) {
		this._label = label;
	}

	get interval(): number {
		return this._interval!;
	}

	set interval(interval: number) {
		this._interval = interval;
	}

	get json(): LayerJson {
		return this._json!;
	}

	public tick(count: number): void {
		if (count % (this._interval ?? 0) === 0) {
			this.updateLayer();
		}
	}

	private updateLayer(): void {
		if (this._updating) {
			return;
		}
		this._updating = true;
		window.fetchJson<LayerJson>(this._url)
			.then((json: LayerJson): void => {
				if (!this._label) {
					// this is the first tick
					this.initial(json);
				}

				// refresh markers
				this.updateMarkers(json);
				this._updating = false;
			})
			.catch((err: unknown): void => {
				this._updating = false;
				console.error(`Error updating markers layer (${this._label})\n`, this, err);
			});
	}

	protected initial(json: object): void {
		const layerJson: LayerJson = json as LayerJson;

		this._label = layerJson.label ?? ''; // set _something_ so we don't keep reloading json every tick
		this._interval = layerJson.interval ?? 300;
		this._defaults = layerJson.defaults;
		this._json = layerJson;

		// merge in the custom layer options
		if (layerJson.options) {
			this.options = {
				...this.options,
				...layerJson.options,
			};

			// create any panes needed for this marker
			this._livemap.createPaneIfNotExist(layerJson.options?.pane);
		}

		// insert any custom css
		if (layerJson.css) {
			document.head.insertAdjacentHTML('beforeend', `<style id='${this.id}'>${layerJson.css}</style>`);
		}

		// create and add the cluster to this layer
		this._cluster = this.createCluster(layerJson);
		this.addLayer(this._cluster);

		// only add to the map if we are not hiding it by default
		if (!layerJson.hidden) {
			// adding to the map makes it visible
			this._livemap.addLayer(this);
		}

		// add this layer to the layers control
		this._livemap.layersControl.addOverlay(this, this._label);
	}

	protected updateMarkers(json: object): void {
		// list of all current markers
		const toRemove: string[] = [...this._markers.keys()];

		// get all markers from json
		const layerJson: LayerJson = json as LayerJson;
		layerJson.markers?.forEach((markerJson: MarkerJson): void => {
			try {
				this.mergeOptions(markerJson);
				let marker: Marker | undefined = this._markers.get(markerJson.id);
				if (!marker) {
					// new marker
					marker = this.createType(markerJson).addTo(this._cluster);
					this._markers.set(markerJson.id, marker);
				} else {
					// existing marker - do not remove
					toRemove.remove(markerJson.id);
				}
				// update marker data
				marker.update(markerJson);
			} catch (e) {
				console.error(`Error refreshing markers in layer (${this._label})\n`, this, markerJson, e);
			}
		});

		// any markers left over are no longer in the json file and must be removed
		toRemove.forEach((key: string): void => {
			this._markers.get(key)?.remove();
			this._markers.delete(key);
		});
	}

	private mergeOptions(json: MarkerJson): void {
		// merge in default options, if any
		if (this._defaults) {
			if (this._defaults.options) {
				json.options = { ...this._defaults.options, ...json.options };
			}
			if (this._defaults.popup) {
				json.popup = { ...this._defaults.popup, ...json.popup };
			}
			if (this._defaults.tooltip) {
				json.tooltip = { ...this._defaults.tooltip, ...json.tooltip };
			}
		}

		// set to correct pane from layer if needed
		const layerPane: string | undefined = this._json?.options?.pane;
		if (layerPane !== undefined && // layer has custom pane set
			json.options?.pane === undefined) {
			// marker does not have custom pane, use layer's instead
			json.options = {
				...json.options,
				pane: layerPane,
			};
		}
	}

	private createType(json: MarkerJson): Marker {
		switch (json.type) {
			case 'circle': {
				return new Circle(this, json);
			}
			case 'ellipse': {
				return new Ellipse(this, json);
			}
			case 'icon': {
				return new Icon(this, json);
			}
			case 'polygon': {
				return new Polygon(this, json);
			}
			case 'polyline': {
				return new Polyline(this, json);
			}
			case 'rectangle': {
				return new Rectangle(this, json);
			}
		}
		throw new Error(`Invalid marker type (${json.type})`);
	}

	private createCluster(layerJson: LayerJson): L.MarkerCluster {
		const clusterOptions: L.MarkerClusterOptions = {
			animate: true,
			animateAddingMarkers: false,
			//iconCreateFunction: (cluster: L.MarkerCluster) => L.divIcon(cluster.getAllChildMarkers()?.[0]?.options?.icon?.options ?? {}),
			iconCreateFunction: (cluster: L.MarkerCluster) => {
				const count: number = cluster.getChildCount();
				const color: string = `color:${(count < 10 ? '#365fab' : (count < 100 ? '#4c71b4' : '#6283bd'))}`;
				const options: L.IconOptions | L.DivIconOptions | undefined = cluster.getAllChildMarkers()?.[0]?.options?.icon?.options;
				return L.divIcon({
					...options,
					html: `<svg preserveAspectRatio='none' width='100%' height='100%' style='${color}'><use href='${options?.iconUrl}'></use></svg><span>${count}</span>`,
				});
			},
			disableClusteringAtZoom: this._livemap.settings.zoom.maxout - 2,
			maxClusterRadius: 80,
			removeOutsideVisibleBounds: true,
			showCoverageOnHover: true,
			spiderfyOnMaxZoom: false,
			zoomToBoundsOnClick: true,
		};

		const pane: string | undefined = layerJson.markers?.[0]?.options?.pane;
		if ((pane?.length ?? 0) > 0) {
			clusterOptions.clusterPane = pane;
		}

		return L.markerClusterGroup(clusterOptions);
	}
}
