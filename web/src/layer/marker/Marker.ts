
import type { Point } from '../../data/Point';
import type { MarkersLayer } from '../MarkersLayer';
import type * as L from 'leaflet';

export interface MarkerJson {
	type: string;
	id: string;
	point: Point;
	points: Point[];
	targetPoint?: Point;
	options: L.MarkerOptions;
	tooltip: L.TooltipOptions;
	popup: PopupOptions;
}

export interface PopupOptions extends L.PopupOptions {
	autoOpen?: boolean | undefined;
}

export abstract class Marker {
	protected readonly _id: string;
	protected readonly _type: string;
	protected readonly _marker: L.Layer;
	protected readonly _layer: L.LayerGroup;

	protected _json: MarkerJson;

	protected constructor(layer: L.LayerGroup, json: MarkerJson, marker: L.Layer) {
		this._id = json.id;
		this._type = json.type;
		this._json = json;
		this._marker = marker;
		this._layer = layer;

		// create any panes needed for this marker
		window.livemap.createPaneIfNotExist(json.options?.pane);
		window.livemap.createPaneIfNotExist(json.popup?.pane);
		window.livemap.createPaneIfNotExist(json.tooltip?.pane);

		// add popup
		if (json.popup) {
			this._marker.bindPopup(json.popup.content as string, json.popup);
		}

		// add tooltip
		if (json.tooltip) {
			this._marker.bindTooltip(json.tooltip.content as string, json.tooltip);
		}
	}

	get id(): string {
		return this._id;
	}

	get type(): string {
		return this._type;
	}

	get json(): MarkerJson {
		return this._json;
	}

	get(): L.Layer {
		return this._marker;
	}

	public addTo(layer: MarkersLayer): Marker {
		// add to marker layer
		this._marker.addTo(layer);

		// popups don't open on their own, so we have to add that ability
		if (this._json?.popup?.autoOpen) {
			this._marker.openPopup();
		}

		return this;
	}

	public remove(): void {
		this._marker.remove();
	}

	public update(json: MarkerJson): void {
		this._json = json;
	}

	public setPane(pane?: string): void {
		if (pane) {
			this._marker.options.pane = pane;
		}
	}
}
