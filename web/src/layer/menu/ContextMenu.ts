import * as L from 'leaflet';

import { MenuRow } from './MenuRow';
import { Point } from '../../data/Point';
import { Url } from '../../data/Url';

import type { Lang } from '../../data/Lang';
import type { LiveMap } from '../../LiveMap';


export class ContextMenu {
	private readonly _livemap: LiveMap;
	private readonly _menu: [HTMLElement, HTMLElement];
	private readonly _rows: MenuRow[];
	private readonly _pointRegex: RegExp = /\[?(-?\d+),(-?\d+)]?/;
	private readonly _format: string = '[ {x}, {z} ]';

	private _point?: Point;
	private _cur: boolean = false;

	public constructor(livemap: LiveMap) {
		this._livemap = livemap;

		// create the menu rows
		this._rows = this.createRows();

		// create dual menus to swap between
		this._menu = [
			this.createMenu(),
			this.createMenu(),
		];

		// setup browser's listeners
		window.addEventListener('blur', document.onblur = (): void => this.close());
		window.addEventListener('contextmenu', (e: MouseEvent) => this.open(e));
		window.addEventListener('keydown', (e: KeyboardEvent) => this.keydown(e));

		// setup map's listeners
		this._livemap.on('load unload resize viewreset move movestart moveend zoom zoomstart zoomend' +
		' zoomlevelschange click dblclick mousedown preclick', (): void => this.close());
	}

	private createRows(): MenuRow[] {
		const lang: Lang = this._livemap.settings.lang;
		const rows: MenuRow[] = [];
		rows.push(new MenuRow('[ 0,0 ]'));
		rows.push(new MenuRow());
		if (navigator.clipboard) {
			// some browsers don't support clipboard api :(
			rows.push(new MenuRow(lang.copy, lang.copyAlt, 'copy', 'Ctrl+C', () => this.copy()));
			rows.push(new MenuRow(lang.paste, lang.pasteAlt, 'paste', 'Ctrl+V', () => this.paste()));
			rows.push(new MenuRow());
			rows.push(new MenuRow(lang.share, lang.shareAlt, 'link', 'Ctrl+S', () => this.share()));
			rows.push(new MenuRow());
		}
		rows.push(new MenuRow(lang.center, lang.centerAlt, 'center', 'F10', () => this.center()));
		return rows;
	}

	private createMenu(): HTMLElement {
		// create the menu
		const wrapper: HTMLElement = L.DomUtil.create('div', 'contextmenu-wrapper');
		const menu: HTMLElement = document.body.appendChild(wrapper)
			.appendChild(L.DomUtil.create('div'))
			.appendChild(L.DomUtil.create('div', 'contextmenu'));

		// add the rows to the menu
		this._rows.forEach((row: MenuRow): void => menu.append(row.create()));

		// prevent menu from opening real context menu
		menu.addEventListener('contextmenu', (e: Event): void => this.stopPropagation(e));

		return wrapper;
	}

	private close(): void {
		// hide the current menu
		this._menu[Number(this._cur)].classList.remove('show');

		this._point = undefined;
	}

	private open(e: MouseEvent): void {
		// close current menu (mobile needs this)
		this.close();

		// show the next menu at mouse position, while keeping it inside the viewable area
		const menu: HTMLElement = this._menu[Number(this._cur = !this._cur)];
		menu.classList.add('show');
		menu.style.top = `${Math.min(window.innerHeight - menu.offsetHeight - 25, e.y)}px`;
		menu.style.left = `${Math.min(window.innerWidth - menu.offsetWidth - 25, e.x)}px`;

		// stop the event from bubbling up the stack
		this.stopPropagation(e);

		// update coordinates in first row
		this._point = this._livemap.coordsControl.getPoint();
		menu.querySelector('div:first-child p:nth-child(2)')!.innerHTML = this._point.toString(this._format);
	}

	private keydown(e: KeyboardEvent): void {
		// close menu when escape is pressed
		if (e.key === 'Escape') {
			this.close();
			return;
		}

		// get current key combo
		const combo: string = this.combo(e);

		// find and run any actions for this key combo
		this._rows.forEach((row: MenuRow): void => {
			if (row.key === combo && row.action) {
				row.action(e);
				this.stopPropagation(e);
			}
		});
	}

	private combo(e: KeyboardEvent): string {
		if (e.key === 'Control' || e.key === 'Shift' || e.key === 'Alt') {
			return '';
		}

		let combo: string = '';
		if (e.ctrlKey) {
			combo += `${combo ? '+' : ''}ctrl`;
		}
		if (e.shiftKey) {
			combo += `${combo ? '+' : ''}shift`;
		}
		if (e.altKey) {
			combo += `${combo ? '+' : ''}alt`;
		}
		combo += `${combo ? '+' : ''}${e.key}`;
		return combo.toLowerCase();
	}

	public copy(): void {
		navigator.clipboard.writeText((this._point ?? this._livemap.coordsControl.getPoint()).toString(this._format))
			.then((): void => {
				this._livemap.notifications.success(this._livemap.settings.lang.notifCopy);
			})
			.catch((e): void => {
				console.error('Could not copy location\n', e);
				this._livemap.notifications.danger(this._livemap.settings.lang.notifCopyFailed);
			})
			.finally((): void => {
				this.close();
			});
	}

	public paste(): void {
		navigator.clipboard.readText()
			.then((text: string): void => {
				const point: Point | undefined = this.pastePoint(text) ?? this.pasteUrl(text);
				if (point) {
					this._livemap.notifications.info(this._livemap.settings.lang.notifPaste);
					this._livemap.centerOn(point);
				} else {
					this._livemap.notifications.warning(this._livemap.settings.lang.notifPasteInvalid);
				}
			})
			.catch((e): void => {
				console.error('Could not paste location\n', e);
				this._livemap.notifications.danger(this._livemap.settings.lang.notifPasteFailed);
			})
			.finally((): void => {
				this.close();
			});
	}

	private pastePoint(text: string): Point | undefined {
		const match: RegExpExecArray | null = this._pointRegex.exec(text.replace(/\s+/g, ''));
		if (match) {
			return Point.of(Number.parseInt(match[1]), Number.parseInt(match[2]));
		}
	}

	private pasteUrl(text: string): Point | undefined {
		try {
			return new Url(this._livemap, text).point;
		} catch {}
	}

	public share(point?: Point): void {
		point ??= this._point ?? this._livemap.coordsControl.getPoint();
		const text: string = `${window.location.origin}${this._livemap.linkControl.getUrlFromPoint(point)}`;
		navigator.clipboard.writeText(text)
			.then((): void => {
				this._livemap.notifications.success(this._livemap.settings.lang.notifShare);
			})
			.catch((e): void => {
				console.error('Could not copy shareable url\n', e);
				this._livemap.notifications.danger(this._livemap.settings.lang.notifShareFailed);
			})
			.finally((): void => {
				this.close();
			});
	}

	public center(): void {
		this._livemap.centerOn(this._livemap.coordsControl.getPoint());
		this._livemap.coordsControl.update();
		this._livemap.linkControl.update();
		this._livemap.notifications.success(this._livemap.settings.lang.notifCenter);
		this.close();
	}

	private stopPropagation(e: Event): void {
		// stop the event in the current element
		// and prevent it from bubbling up the stack
		e.preventDefault();
		e.stopPropagation();
		e.stopImmediatePropagation();
	}
}
