import * as L from 'leaflet';

import { Renderer } from '../data/Renderer';

import type { LiveMap } from '../LiveMap';

export class RenderersControl {
	private readonly _livemap: LiveMap;
	private readonly _dom: HTMLElement;

	private _renderers: Renderer[] = [];

	private _rendererType: string = 'basic';

	constructor(livemap: LiveMap) {
		this._livemap = livemap;

		this._dom = L.DomUtil.create('ul');

		livemap.settings.renderers.forEach((renderer: Renderer): void => {
			this._renderers.push(new Renderer(renderer));

			const li: HTMLLIElement = L.DomUtil.create('li', '', this._dom);
			li.id = `renderer-${renderer.id}`;
			li.title = renderer.id;

			const iconName = (renderer.icon || renderer.id).toLowerCase();
			const icon: DocumentFragment = window.createSVGIcon(iconName);
			li.append(icon);

			const p: HTMLParagraphElement = L.DomUtil.create('p', '', li);
			p.innerText = renderer.id.charAt(0).toUpperCase() + renderer.id.slice(1);

			li.addEventListener('click', (): void => {
				this.rendererType = renderer.id;
				this._update();
			});
		});

		this._update();
	}

	private _update(): void {
		const currentRenderer = this.rendererType;

		[...this._dom.children].forEach((child: Element) => {
			if (child.id === `renderer-${currentRenderer}`) {
				child.classList.add('active');
			} else {
				child.classList.remove('active');
			}
		});

		const url = new URL(window.location.href);
		url.searchParams.set('renderer', currentRenderer);
		window.history.pushState({}, '', url.toString());

		this._livemap.tileLayerControl.updateTileLayer();
	}

	get dom(): HTMLElement {
		return this._dom;
	}

	get rendererType(): string {
		return this._rendererType;
	}

	set rendererType(renderer: string | null) {
		if (!renderer?.length || renderer === 'unknown') {
			this._rendererType = this._renderers[0]?.id ?? 'basic';
		} else {
			this._rendererType = renderer;
		}
	}
}
