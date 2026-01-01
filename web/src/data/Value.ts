// todo - get a better name than this
export class Value {
	private readonly _cur: number;
	private readonly _max: number;

	constructor(value?: Value) {
		this._cur = value?.cur ?? 0;
		this._max = value?.max ?? 0;
	}

	get cur(): number {
		return this._cur;
	}

	get max(): number {
		return this._max;
	}
}
