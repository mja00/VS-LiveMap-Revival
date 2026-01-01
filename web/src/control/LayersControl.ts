import * as L from 'leaflet';

import { MarkersLayer } from '../layer/MarkersLayer';

import type { LiveMap } from '../LiveMap';

interface LayersJson {
	markers: string[];
}

export class LayersControl extends L.Control.Layers {
	declare _layers: L.Control.LayersObject[];

	constructor(livemap: LiveMap) {
		super({}, {}, {
			position: 'topleft',
		});

		this.addTo(livemap);

		window.fetchJson<LayersJson>('data/markers.json')
			.then((json: LayersJson): void => {
				// each layer has its own json file full of markers
				json.markers.forEach((layer: string): void => {
					try {
						new MarkersLayer(livemap, `data/markers/${layer}.json`);
					} catch (e) {
						console.error(`Error loading markers layer (${layer})\n`, e);
					}
				});
			})
			.catch((err: unknown): void => {
				console.error('Error fetching marker layers\n', this, err);
			});
	}

	public tick(count: number): void {
		this._layers.forEach((obj: L.Control.LayersObject): void => {
			try {
				(obj.layer as MarkersLayer).tick(count);
			} catch (e) {
				console.error('Error ticking markers layer\n', obj.layer, e);
			}
		});
	}
}
