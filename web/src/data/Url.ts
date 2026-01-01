import { Point } from './Point';

import type { LiveMap } from '../LiveMap';

export class Url {
	private readonly _livemap: LiveMap;
	private readonly _basePath: string;
	private readonly _renderer: string;
	private readonly _zoom: number;
	private readonly _point: Point;

	constructor(livemap: LiveMap, url: string, renderer?: string | null, zoom?: string | number | null, x?: string | number | null, z?: string | number | null) {
		this._livemap = livemap;

		if (renderer) {
			this._basePath = url ?? '/';
		} else {
			const match: RegExpExecArray | null = /(.*\/)?(.+)\/([+-]?\d+)\/([+-]?\d+)\/([+-]?\d+)(\/.*)?$/.exec(url);
			if (match) {
				this._basePath = match[1] ?? '/';
				renderer = match[2];
				zoom = match[3];
				x = match[4];
				z = match[5];
			} else {
				this._basePath = window.location.pathname?.split('?')[0]?.replace('index.html', '') ?? '/';
				const url: URLSearchParams = new URLSearchParams(window.location.search);
				renderer = url.get('renderer');
				zoom = url.get('zoom');
				x = url.get('x');
				z = url.get('z');
			}
		}

		const defaultRenderer = this._livemap.settings.renderers?.[0]?.id ?? 'basic';
		this._renderer = renderer ?? defaultRenderer;
		this._zoom = Number(zoom ?? this._livemap.settings.zoom.def);
		this._point = Point.of(x ?? 0, z ?? 0);
	}

	get basePath(): string {
		return this._basePath;
	}

	get renderer(): string {
		return this._renderer;
	}

	get zoom(): number {
		return this._zoom;
	}

	get x(): number {
		return this._point.x;
	}

	get z(): number {
		return this._point.z;
	}

	get point(): Point {
		return this._point;
	}

	public toString(): string {
		if (this._livemap.settings.friendlyUrls) {
			return `${this.basePath}${this.renderer}/${this.zoom}/${this.point.x}/${this.point.z}/`;
		}
		return `${this.basePath}?renderer=${this.renderer}&zoom=${this.zoom}&x=${this.point.x}&z=${this.point.z}`;
	}
}
