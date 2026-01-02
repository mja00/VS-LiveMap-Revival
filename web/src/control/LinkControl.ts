import * as L from 'leaflet';

import { ControlBox } from './ControlBox';
import { Point } from '../data/Point';
import { Url } from '../data/Url';

import type { LiveMap } from '../LiveMap';

export class LinkControl extends ControlBox {
	private readonly _dom: HTMLAnchorElement;
	private readonly _url: Url;

	constructor(livemap: LiveMap) {
		super(livemap, 'bottomleft');

		this._dom = L.DomUtil.create('a', 'leaflet-control-layers link');
		this._dom.title = livemap.settings.lang.shareTitle;
		this._dom.append(window.createSVGIcon('link'));
		L.DomEvent.disableClickPropagation(this._dom);
		this._dom.addEventListener('click', (e: MouseEvent): void => {
			e.preventDefault();
			window.history.replaceState({}, 'LiveMap', this._dom.href);
			this._livemap.contextMenu.share(
				Point.of(this._livemap.getCenter())
					.floor()
					.subtract(this._livemap.settings.spawn),
			);
		});

		// add to the map once we have a dom to add
		this.addTo(livemap);

		// parse data from the browser's url
		this._url = new Url(this._livemap, window.location.pathname);

		// center the map on url coordinates or spawn (0, 0);
		// this sets up the map after ctor and before load
		// onLoad will not call until this is finished
		setTimeout((): void => {
			this._livemap.sidebarControl.renderersControl.rendererType = this._url.renderer;
			this._livemap.centerOn(this._url.point, this._url.zoom);
		}, 0);
	}

	onAdd(map: L.Map): HTMLAnchorElement {
		map.addEventListener('moveend', this.update);
		map.addEventListener('zoomend', this.update);
		return this._dom;
	}

	onRemove(map: L.Map): void {
		map.removeEventListener('moveend', this.update);
		map.removeEventListener('zoomend', this.update);
	}

	update = (): void => {
		this._dom.href = this.getUrlFromView().toString();
		// todo - find out how to prevent chrome from spamming history
		window.history.replaceState({}, '', this._dom.href);
	};

	public getUrlFromView(): Url {
		return this.getUrlFromPoint(
			Point.of(this._livemap.getCenter()).floor()
				.subtract(this._livemap.settings.spawn),
		);
	}

	public getUrlFromPoint(point: Point): Url {
		return new Url(this._livemap, this._url.basePath,
			this._livemap.sidebarControl.renderersControl.rendererType,
			this._livemap.currentZoom(), point.x, point.z);
	}
}
