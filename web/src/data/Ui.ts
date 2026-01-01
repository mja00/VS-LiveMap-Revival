export class Ui {
	private readonly _attribution: string;
	private readonly _logolink: string;
	private readonly _logoimg: string;
	private readonly _logotext: string;
	private readonly _sitetitle: string;
	private readonly _sidebar: string;

	constructor(json?: Ui) {
		this._attribution = json?.attribution ?? '<a href=\'https://mods.vintagestory.at/livemap\' target=\'_blank\'>Livemap</a> &copy;2024';
		this._logolink = json?.logolink ?? 'https://mods.vintagestory.at/livemap';
		this._logoimg = json?.logoimg ?? '<svg xmlns=\'http://www.w3.org/2000/svg\' viewBox=\'0 0 100 100\' fill=\'none\' stroke=\'currentColor\' stroke-width=\'2\' stroke-linecap=\'round\' stroke-linejoin=\'round\'><path fill=\'currentColor\' d=\'m2 2 32 16v80l-32-16v-80z\'></path><path d=\'m34 18 32-16 32 16v80l-32-16-32 16\'></path><path d=\'m66 8v68\'></path></svg>';
		this._logotext = json?.logotext ?? 'LiveMap';
		this._sitetitle = json?.sitetitle ?? 'Vintage Story LiveMap';
		this._sidebar = json?.sidebar ?? 'unpinned';
	}

	get attribution(): string {
		return this._attribution;
	}

	get logolink(): string {
		return this._logolink;
	}

	get logoimg(): string {
		return this._logoimg;
	}

	get logotext(): string {
		return this._logotext;
	}

	get sitetitle(): string {
		return this._sitetitle;
	}

	get sidebar(): string {
		return this._sidebar;
	}
}
