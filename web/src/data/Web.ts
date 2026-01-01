export class Web {
	private readonly _tiletype: string;

	constructor(web?: Web) {
		this._tiletype = web?._tiletype ?? 'webp';
	}

	get tiletype(): string {
		return this._tiletype;
	}
}
