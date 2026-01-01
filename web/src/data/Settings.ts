import { Lang } from './Lang';
import { Point } from './Point';
import { Renderer } from './Renderer';
import { Ui } from './Ui';
import { Web } from './Web';
import { Zoom } from './Zoom';

export class Settings {
	private readonly _friendlyUrls: boolean;
	private readonly _playerList: boolean;
	private readonly _playerMarkers: boolean;
	private readonly _maxPlayers: number;
	private readonly _interval: number;
	private readonly _size: Point;
	private readonly _spawn: Point;
	private readonly _web: Web;
	private readonly _zoom: Zoom;
	private readonly _renderers: Renderer[];
	private readonly _ui: Ui;
	private readonly _lang: Lang;

	constructor(json: Settings) {
		this._friendlyUrls = json.friendlyUrls ?? true;
		this._playerList = json.playerList ?? true;
		this._playerMarkers = json.playerMarkers ?? true;
		this._maxPlayers = json.maxPlayers ?? 0;
		this._interval = json.interval ?? 30;
		// @ts-expect-error - typescript struggles here ig
		this._size = json.size > 0 ? Point.of(json.size) : Point.of(1_024_000, 1_024_000);
		this._spawn = json.spawn ? Point.of(json.spawn) : this.size.divide(2);
		this._web = json.web ? new Web(json.web) : new Web();
		this._zoom = json.zoom ? new Zoom(json.zoom) : new Zoom();
		this._renderers = (json.renderers ?? []).map((renderer: any) => new Renderer(renderer));
		this._ui = json.ui ? new Ui(json.ui) : new Ui();
		this._lang = new Lang(json.lang);
	}

	get friendlyUrls(): boolean {
		return this._friendlyUrls;
	}

	get playerList(): boolean {
		return this._playerList;
	}

	get playerMarkers(): boolean {
		return this._playerMarkers;
	}

	get maxPlayers(): number {
		return this._maxPlayers;
	}

	get interval(): number {
		return this._interval;
	}

	get size(): Point {
		return this._size;
	}

	get spawn(): Point {
		return this._spawn;
	}

	get web(): Web {
		return this._web;
	}

	get zoom(): Zoom {
		return this._zoom;
	}

	get renderers(): Renderer[] {
		return this._renderers;
	}

	get ui(): Ui {
		return this._ui;
	}

	get lang(): Lang {
		return this._lang;
	}
}
