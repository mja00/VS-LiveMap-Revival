export class Zoom {
	private readonly _def: number;
	private readonly _maxin: number;
	private readonly _maxout: number;

	constructor(zoom?: Zoom) {
		this._def = zoom?.def ?? 0;
		this._maxin = zoom?.maxin ?? -3;
		this._maxout = zoom?.maxout ?? 8;
	}

	get def(): number {
		return this._def;
	}

	get maxin(): number {
		return this._maxin;
	}

	get maxout(): number {
		return this._maxout;
	}
}
