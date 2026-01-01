import type { Player } from './Player';

export class Players {
	private readonly _interval: number;
	private readonly _hidden: boolean;
	private readonly _players: Player[];

	constructor(json?: Players) {
		this._interval = json?.interval ?? 1;
		this._hidden = json?.hidden ?? false;
		this._players = json?.players ?? [];
	}

	get interval(): number {
		return this._interval;
	}

	get hidden(): boolean {
		return this._hidden;
	}

	get players(): Player[] {
		return this._players;
	}
}
