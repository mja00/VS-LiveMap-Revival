import * as L from 'leaflet';

import { PinControl } from './PinControl';
import { RenderersControl } from './RenderersControl';

import type { LiveMap } from '../LiveMap';

export class SidebarControl {
	private readonly _livemap: LiveMap;
	private readonly _dom: HTMLElement;

	private readonly _renderersControl: RenderersControl;

	constructor(livemap: LiveMap) {
		this._livemap = livemap;

		this._renderersControl = new RenderersControl(this._livemap);

		this._dom = L.DomUtil.create('aside');

		if (livemap.settings.ui.sidebar !== 'hide') {
			document.body.prepend(this._dom);
		}

		// set up and show/hide the pin
		const pin: PinControl = new PinControl(this._livemap, this._dom);
		this.show(pin.pinned);

		// set up the logo fancy div magic
		const holder: HTMLElement = L.DomUtil.create('div', '', this._dom);
		const logo: HTMLElement = L.DomUtil.create('div', '', holder);
		L.DomUtil.create('div', '', holder); // this one is squishy :3

		const logolink: string = this._livemap.settings.ui.logolink;
		const logotext: string = this._livemap.settings.ui.logotext;

		let rawhtml: string = this._livemap.settings.ui.logoimg;
		if (logolink) {
			rawhtml += `<span>${logotext}</span>`;
		} else {
			rawhtml += `<a href='${logolink}'>${logotext}</a>`;
		}
		rawhtml += '<span></span>';
		logo.insertAdjacentHTML('beforeend', rawhtml);

		// add these after the logo
		this._dom.append(this.renderersControl.dom);
		this._dom.append(this._livemap.playersLayer.dom);

		this._dom.addEventListener('click', (): void => {
			// todo followPlayerMarker(null)
		});
		this._dom.addEventListener('mouseleave', (): void => {
			if (!pin.pinned) {
				this.show(false);
			}
		});
		this._dom.addEventListener('mouseenter', (): void => {
			if (!pin.pinned) {
				this.show(true);
			}
		});
	}

	get renderersControl(): RenderersControl {
		return this._renderersControl;
	}

	public show(show: boolean): void {
		this._dom.className = show ? 'show' : '';
	}
}
