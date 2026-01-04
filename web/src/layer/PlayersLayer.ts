import * as L from 'leaflet';

import { MarkersLayer } from './MarkersLayer';
import { Player } from '../data/Player';
import { Point } from '../data/Point';
import { Icon } from './marker/Icon';

import type { Players } from '../data/Players';
import type { Value } from '../data/Value';
import type { LiveMap } from '../LiveMap';
import type { Marker, MarkerJson } from './marker/Marker';

export class PlayersLayer extends MarkersLayer {
	private readonly _dom: HTMLElement;

	private readonly _players: Map<string, Player> = new Map();

	private _max: number = 0;

	private _activePlayerElement: HTMLElement | null = null;

	constructor(livemap: LiveMap) {
		super(livemap, 'data/players.json');
		this._dom = L.DomUtil.create('ul');

		this.options = {
			...this.options,
			pane: 'players',
		};
	}

	get dom(): HTMLElement {
		return this._dom;
	}

	get cur(): number {
		return this._players.size;
	}

	get max(): number {
		return this._max;
	}

	get players(): IterableIterator<Player> {
		return this._players.values();
	}

	protected override initial(json: object): void {
		const players: Players = json as Players;

		this.label = this._livemap.settings.lang.players;
		this.interval = players.interval;
		this._livemap.layersControl.addOverlay(this, this.label);
		if (!players.hidden && !this._livemap.hasLayer(this)) {
			this._livemap.addLayer(this);
		}
	}

	protected override updateMarkers(json: object): void {
		const players: Players = json as Players;

		// update players in marker layer and sidebar
		this.updatePlayers(players);

		// todo update player counts in sidebar legend
		/*this._legend.textContent = this._livemap.settings.lang.players
			.replace(/{cur}/g, this.cur.toString())
			.replace(/{max}/g, this.max.toString());/*/

		// todo follow highlighted player
		//
	}

	private updatePlayers(json: Players): void {
		const toRemove: Set<string> = new Set(this._players.keys());

		json.players.forEach((data: Player): void => {
			const player: Player | undefined = this._players.get(data.name);
			if (player) {
				this.updatePlayer(player, new Player(data));
				toRemove.delete(player.name);
			} else {
				this.createPlayer(new Player(data));
			}
		});

		toRemove.forEach((name: string): void => {
			this.removePlayer(name);
		});
	}

	private createPlayer(player: Player): void {
		this._players.set(player.name, player);
		if (this._livemap.settings.playerMarkers) {
			this.addMarker(player);
		}
		if (this._livemap.settings.playerList) {
			this.addToSidebar(player);
		}
	}

	private updatePlayer(player: Player, data: Player): void {
		player.updateData(data);
		const icon: Icon = this.markers.get(player.name) as Icon;
		const marker: L.Marker = icon.get() as L.Marker;
		marker.setLatLng(data.pos.toLatLng());
		marker.setTooltipContent(this.tooltip(data));

		// https://stackoverflow.com/a/53416030/3530727
		const from: number = marker.options.rotationAngle ?? Math.round(data.yaw);
		const to: number = Math.round(data.yaw);
		const diff: number = ((((to - from) % 360) + 540) % 360) - 180;
		marker.options.rotationAngle = from + diff;
	}

	private removePlayer(name: string): void {
		this._players.delete(name);
		this.removeMarker(name);
		this.removeFromSidebar(name);
	}

	private addMarker(player: Player): void {
		const icon: Icon = new Icon(this, {
			type: 'icon',
			id: player.name,
			point: player.pos,
			options: {
				title: player.name,
				rotationAngle: player.yaw,
				iconUrl: '#svg-player',
				iconSize: [24, 24],
				iconAnchor: [12, 12],
			},
			tooltip: {
				permanent: true,
				direction: 'right',
				offset: [10, 0],
				pane: 'players',
				content: this.tooltip(player),
			},
		} as unknown as MarkerJson);
		icon.addTo(this);
		this.markers.set(player.name, icon);
	}

	private removeMarker(name: string): void {
		const marker: Marker | undefined = this.markers.get(name);
		if (marker) {
			this.markers.delete(name);
			marker.remove();
		}
	}

	private addToSidebar(player: Player): void {
		const li: HTMLElement = L.DomUtil.create('li', '', this._dom);
		li.id = player.name;
		li.title = player.name;

		const img: HTMLImageElement = L.DomUtil.create('img', '', li);
		img.src = player.avatar;
		img.alt = this._livemap.settings.lang.avatarAlt.replace('<player>', player.name);

		const p: HTMLParagraphElement = L.DomUtil.create('p', '', li);
		if (player.color?.length > 0) {
			p.style.color = player.color;
			p.style.fontWeight = '700';
			p.style.textShadow = '1px 1px 2px #000000E5';
		}
		p.innerText = player.name;

		// Add click handler to focus camera on player
		li.addEventListener('click', (): void => {
			const clickedPlayer: Player | undefined = this._players.get(player.name);
			if (clickedPlayer) {
				// Remove active class from previously active player
				if (this._activePlayerElement) {
					this._activePlayerElement.classList.remove('active');
				}

				// Add active class to clicked player
				li.classList.add('active');
				this._activePlayerElement = li;

				// Center camera on player position (convert from absolute to relative to spawn)
				const relativePos = Point.of(clickedPlayer.pos).subtract(this._livemap.settings.spawn);
				this._livemap.centerOn(relativePos);
			}
		});
	}

	private removeFromSidebar(name: string): void {
		this._dom.querySelectorAll(`#${name}`)?.forEach((li: Element): void => {
			// Clear active player if this is the active one
			if (this._activePlayerElement === li) {
				this._activePlayerElement = null;
			}
			li.remove();
		});
	}

	private tooltip(player: Player): string {
		const nameStyle: string = player.color?.length > 0 ? ` style='color:${player.color};font-weight:700;text-shadow:1px 1px 2px #000000E5'` : '';
		return `<ul><li><img src='${player.avatar}' alt='${this._livemap.settings.lang.avatar}'></li><li${nameStyle}>${player.name}<div style='${this.stat(player.health, 3)}'><p></p></div><div style='${this.stat(player.satiety, 300)};--height:6px'><p></p></div></li></ul>`;
	}

	private stat(value: Value, divisor: number): string {
		const segments: number = Math.round(value.max / divisor);
		const amount: number = Math.round((Math.min(value.cur, value.max) / value.max) * 100);
		return `--amount:${amount}%;--segments:${segments}`;
	}
}
