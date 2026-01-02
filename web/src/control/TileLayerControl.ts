import { LiveTileLayer } from '../layer/LiveTileLayer';

import type { LiveMap } from '../LiveMap';

export class TileLayerControl {
	private readonly _livemap: LiveMap;

	private readonly _layers: LiveTileLayer[] = [];

	private _cur: boolean = false;
	private _updating: boolean = false;

	constructor(livemap: LiveMap) {
		this._livemap = livemap;

		// we need 2 tile layers to swap between for seamless refreshing
		livemap.addLayer(this._layers[0] = this.createTileLayer(livemap));
		livemap.addLayer(this._layers[1] = this.createTileLayer(livemap));
	}

	public tick(count: number): void {
		if (this._updating) {
			return;
		}
		if (count % this._livemap.settings.interval === 0) {
			try {
				this._updating = true;
				this.updateTileLayer();
			} catch (err) {
				console.error(err);
			}
			this._updating = false;
		}
	}

	private createTileLayer(livemap: LiveMap): LiveTileLayer {
		return new LiveTileLayer(livemap)
			.addEventListener('load', (): void => {
				// switch layers when all tiles are loaded
				this.switchTileLayer();
			});
	}

	private switchTileLayer(): void {
		// swap tile layers
		this._layers[Number(this._cur)].setZIndex(0);
		this._layers[Number(!this._cur)].setZIndex(1);
		this._cur = !this._cur;
	}

	public updateTileLayer(): void {
		// redraw (reload images) hidden tile layer to prepare for swap
		// we target the hidden layer (!this._cur) so it loads in the background
		// once loaded, the 'load' event triggers switchTileLayer(), making it visible
		this._layers[Number(!this._cur)].redraw();
	}
}
