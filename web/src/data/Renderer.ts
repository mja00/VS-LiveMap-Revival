export class Renderer {
	private readonly _id: string;
	private readonly _icon: string;

	constructor(renderer?: Renderer) {
		this._id = renderer?.id ?? '';
		this._icon = renderer?.icon ?? '';
	}

	get id(): string {
		return this._id;
	}

	get icon(): string {
		return this._icon;
	}
}
