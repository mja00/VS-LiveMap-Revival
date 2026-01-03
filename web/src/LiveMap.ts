import * as L from 'leaflet';

import { CoordsControl } from './control/CoordsControl';
import { LayersControl } from './control/LayersControl';
import { LinkControl } from './control/LinkControl';
import { SidebarControl } from './control/SidebarControl';
import { TileLayerControl } from './control/TileLayerControl';
import { Settings } from './data/Settings';
import { ContextMenu } from './layer/menu/ContextMenu';
import { Notifications } from './layer/Notifications';
import { PlayersLayer } from './layer/PlayersLayer';

import type { Point } from './data/Point';
import './scss/styles';
import './svg';

export class LiveMap extends L.Map {
	declare _controlCorners: { [x: string]: HTMLDivElement; };
	declare _controlContainer?: HTMLElement;
	declare _container?: HTMLElement;

	private readonly _settings: Settings;

	private readonly _tileLayerControl: TileLayerControl;
	private readonly _playersLayer: PlayersLayer;
	private readonly _layersControl: LayersControl;
	private readonly _linkControl: LinkControl;
	private readonly _coordsControl: CoordsControl;
	private readonly _sidebarControl: SidebarControl;

	private readonly _contextMenu: ContextMenu;
	private readonly _notifications: Notifications;

	private readonly _scale: number;

	constructor(settings: Settings) {
		// create the map div element
		L.DomUtil.create('div', 'loading', document.body).id = 'map';

		super('map', {
			// we need a flat and simple crs
			crs: L.Util.extend(L.CRS.Simple, {
				// we need to flip the y-axis correctly
				// https://stackoverflow.com/a/62320569/3530727
				transformation: new L.Transformation(1, 0, 1, 0),
			}),
			// center map on spawn
			center: [settings.spawn.x, settings.spawn.z],
			// always allow attribution in case a layer needs it
			attributionControl: true,
			// canvas is more efficient than svg
			preferCanvas: true,
			// these get weird when changed
			zoomSnap: 1,
			zoomDelta: 1,

			// chrome based browsers on linux zoom twice as fast, so we have to double the ratio
			// effectively undoes the fix for Leaflet/Leaflet#4538 and Leaflet/Leaflet#7403
			// https://github.com/Leaflet/Leaflet/commit/96977a19358374b0166cff049862fa1f0fed5948
			//
			// todo remove this logic when this bug gets fixed: https://issues.chromium.org/issues/40887377
			// it seems intentional, so it might not get fixed https://issues.chromium.org/issues/40804672
			zoomControl: false,
		});
		window.livemap = this;

		this._settings = settings;

		L.control.zoom({
			zoomInTitle: settings.lang.zoomIn,
			zoomOutTitle: settings.lang.zoomOut,
		}).addTo(this);

		// set custom page title from lang
		if (document.title.trim() === '') {
			document.title = settings.ui.sitetitle ?? 'Vintage Story LiveMap';
		}

		// pre-calculate map's scale
		this._scale = (1 / (2 ** settings.zoom.maxout));

		// set up the controllers
		this._tileLayerControl = new TileLayerControl(this);
		this._layersControl = new LayersControl(this);
		this._playersLayer = new PlayersLayer(this);
		this._coordsControl = new CoordsControl(this);
		this._linkControl = new LinkControl(this);
		this._sidebarControl = new SidebarControl(this);

		// the fancy context menu and stuff
		this._contextMenu = new ContextMenu(this);
		this._notifications = new Notifications();

		// replace leaflet's attribution with our own
		this.attributionControl.setPrefix(settings.ui.attribution);

		// stuff to do after the map fully loads
		this.on('load', (): void => this.onLoad());
	}

	onLoad(): void {
		// get rid of the page logo and loading images
		const container: HTMLElement = this.getContainer();
		container.classList.remove('loading');
		container.addEventListener('transitionend', (ev: TransitionEvent): void => {
			if (ev.target === container) {
				document.querySelector('.logo')?.remove();
			}
		}, { passive: true });

		// fix map size on load - fixes android browser url bar pushing page off-screen
		// https://chanind.github.io/javascript/2019/09/28/avoid-100vh-on-mobile-web.html
		this.updateSizeToWindow();

		// replace layers.png with an svg
		const layers: HTMLElement = document.querySelector('.leaflet-control-layers-toggle')!;
		layers.append(window.createSVGIcon('layers'));
		const svg: SVGElement = layers.firstChild as SVGElement;
		svg.style.width = '24px';
		svg.style.height = '24px';
		svg.style.margin = '3px';

		// fix svg size issues in weird browsers like safari
		document.querySelectorAll('svg').forEach((svg: Element): void => {
			svg.setAttribute('preserveAspectRatio', 'none');
		});

		// start the tick loop
		this.loop(0);
	}

	// https://stackoverflow.com/a/60391674/3530727
	_initControlPos(): void {
		const container: HTMLDivElement = this._controlContainer = L.DomUtil.create('div', 'leaflet-control-container', this._container);
		const corners: { [x: string]: HTMLDivElement; } = this._controlCorners = {};

		function createRow(vSide: string): void {
			const div: HTMLDivElement = L.DomUtil.create('div', `leaflet-control-container-${vSide}`, container);
			createCell(vSide, 'left', div);
			createCell(vSide, 'center', div);
			createCell(vSide, 'right', div);
		}

		function createCell(vSide: string, hSide: string, container: HTMLDivElement): void {
			corners[`${vSide}${hSide}`] = L.DomUtil.create('div', `leaflet-${vSide} leaflet-${hSide}`, container);
		}

		createRow('top');
		createRow('middle');
		createRow('bottom');
	}

	get settings(): Settings {
		return this._settings;
	}

	get tileLayerControl(): TileLayerControl {
		return this._tileLayerControl;
	}

	get playersLayer(): PlayersLayer {
		return this._playersLayer;
	}

	get layersControl(): LayersControl {
		return this._layersControl;
	}

	get coordsControl(): CoordsControl {
		return this._coordsControl;
	}

	get linkControl(): LinkControl {
		return this._linkControl;
	}

	get sidebarControl(): SidebarControl {
		return this._sidebarControl;
	}

	get contextMenu(): ContextMenu {
		return this._contextMenu;
	}

	get notifications(): Notifications {
		return this._notifications;
	}

	get scale(): number {
		return this._scale;
	}

	private loop(count: number): void {
		try {
			if (document.visibilityState === 'visible') {
				this.tileLayerControl.tick(count);
				this.layersControl.tick(count);
				// Only schedule next tick when page is visible
				setTimeout(() => this.loop(++count), 1000);
			} else {
				// Page is hidden, wait for visibility change to resume
				const resume = () => {
					this.loop(count); // Resume with same count
				};
				document.addEventListener('visibilitychange', resume, { once: true });
			}
		} catch (err) {
			console.error(`Error processing tick (${count})\n`, err);
			// Continue loop even on error, but wait longer
			const nextCount = count + 1;
			if (document.visibilityState === 'visible') {
				setTimeout(() => this.loop(nextCount), 5000);
			} else {
				// Page is hidden during error; wait for visibility change before retrying
				const resumeOnError = () => {
					setTimeout(() => this.loop(nextCount), 5000);
				};
				document.addEventListener('visibilitychange', resumeOnError, { once: true });
			}
		}
	}

	public createPaneIfNotExist(pane?: string): void {
		if (pane && this.getPane(pane) === undefined) {
			this.createPane(pane);
		}
	}

	public centerOn(point: Point, zoom?: number | string): void {
		if (zoom !== undefined) {
			this.setZoom(this.settings.zoom.maxout - Number(zoom));
		}
		this.setView(point.add(this.settings.spawn).toLatLng());
		this._linkControl.update();
	}

	public currentZoom(): number {
		return this.settings.zoom.maxout - this.getZoom();
	}

	public updateSizeToWindow(): void {
		const style: CSSStyleDeclaration = this.getContainer().style;
		style.width = `${window.innerWidth}px`;
		style.height = `${window.innerHeight}px`;
		this.invalidateSize();
	}
}

window.addEventListener('load', (): void => {
	window.fetchJson<Settings>('data/settings.json')
		.then((json: Settings): void => {
			new LiveMap(new Settings(json));
		})
		.catch((err: unknown): void => {
			console.error('Error creating map\n', err);
		});
});

// update map size when window size, scale, or orientation changes
'orientationchange resize'.split(' ').forEach((event: string): void => {
	window.addEventListener(event, (): void => {
		window.livemap?.updateSizeToWindow();
	}, { passive: true });
});

window.fetchJson = async <T>(url: string): Promise<T> => {
	const res: Response = await fetch(url, {
		headers: {
			'Content-Disposition': 'inline',
		},
	});
	if (res.ok) {
		return await res.json();
	}
	throw (res.statusText);
};

window.createSVGIcon = (icon: string): DocumentFragment => {
	const template: HTMLTemplateElement = L.DomUtil.create('template');
	template.innerHTML = `<svg><use href='#svg-${icon}'></use></svg>`;
	return template.content;
};




// Hardcoded theme list instead of parsing stylesheets on every page load
// Update this list when adding new themes to themes.css
const knownThemes: string[] = ['white-glass', 'black-glass', 'clear-glass', 'light', 'dark'];

window.matchMedia('(prefers-color-scheme: dark)')
	.addEventListener('change', (): void => setTheme());

const setTheme = (): void => {
	const prefersDark: boolean = knownThemes.length > 1 && window.matchMedia('(prefers-color-scheme: dark)').matches;
	const theme: string = localStorage.getItem('theme') ?? knownThemes[Number(prefersDark)];
	document.querySelector('html')!.setAttribute('theme', theme);
	// todo
	//localStorage.setItem('theme', theme);
	//localStorage.removeItem('theme');

	const icon: HTMLLinkElement | null = document.querySelector('link[rel=\'shortcut icon\']');
	if (icon) {
		icon.href = prefersDark ? 'favicon-white.ico' : 'favicon.ico';
	}
};

setTheme();
