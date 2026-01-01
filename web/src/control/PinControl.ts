import * as L from 'leaflet';

import type { LiveMap } from '../LiveMap';

export class PinControl {
	private readonly _livemap: LiveMap;
	private readonly _dom: HTMLElement;
	private readonly _svg: SVGSVGElement;

	private _pinned: boolean = false;

	constructor(livemap: LiveMap, parent: HTMLElement) {
		this._livemap = livemap;

		this._dom = L.DomUtil.create('div', '', parent);
		this._dom.id = 'pin';
		this._dom.addEventListener('click', (): void => {
			this.pin(!this.pinned);
			localStorage.setItem('pinned', this.pinned ? 'pinned' : 'unpinned');
		});

		this._dom.append(window.createSVGIcon('pin'));
		this._svg = this._dom.querySelector('svg')!;

		this.pin(livemap.settings.ui.sidebar === 'pinned' || localStorage.getItem('pinned') === 'pinned');
	}

	public get pinned(): boolean {
		return this._pinned;
	}

	public pin(pinned: boolean): void {
		this._pinned = pinned;

		this._dom.className = pinned ? 'pinned' : 'unpinned';
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		const text: string = (this._livemap.settings.lang as any)[this._dom.className];

		this._svg.setAttribute('alt', text);
		this._svg.setAttribute('title', text);
	}
}
