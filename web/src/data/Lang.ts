export class Lang {
	private readonly _pinned: string;
	private readonly _unpinned: string;
	private readonly _players: string;
	private readonly _renderers: string;
	private readonly _copy: string;
	private readonly _copyAlt: string;
	private readonly _paste: string;
	private readonly _pasteAlt: string;
	private readonly _share: string;
	private readonly _shareAlt: string;
	private readonly _center: string;
	private readonly _centerAlt: string;
	private readonly _notifCopy: string;
	private readonly _notifCopyFailed: string;
	private readonly _notifPaste: string;
	private readonly _notifPasteFailed: string;
	private readonly _notifPasteInvalid: string;
	private readonly _notifShare: string;
	private readonly _notifShareFailed: string;
	private readonly _notifCenter: string;

	constructor(lang: Lang) {
		this._pinned = lang.pinned;
		this._unpinned = lang.unpinned;
		this._players = lang.players;
		this._renderers = lang.renderers;
		this._copy = lang.copy;
		this._copyAlt = lang.copyAlt;
		this._paste = lang.paste;
		this._pasteAlt = lang.pasteAlt;
		this._share = lang.share;
		this._shareAlt = lang.shareAlt;
		this._center = lang.center;
		this._centerAlt = lang.centerAlt;
		this._notifCopy = lang.notifCopy;
		this._notifCopyFailed = lang.notifCopyFailed;
		this._notifPaste = lang.notifPaste;
		this._notifPasteFailed = lang.notifPasteFailed;
		this._notifPasteInvalid = lang.notifPasteInvalid;
		this._notifShare = lang.notifShare;
		this._notifShareFailed = lang.notifShareFailed;
		this._notifCenter = lang.notifCenter;
	}

	get pinned(): string {
		return this._pinned ?? 'Pinned';
	}

	get unpinned(): string {
		return this._unpinned ?? 'Unpinned';
	}

	get players(): string {
		return this._players ?? 'Players';
	}

	get renderers(): string {
		return this._renderers ?? 'Map Types';
	}

	get copy(): string {
		return this._copy ?? 'Copy';
	}

	get copyAlt(): string {
		return this._copyAlt ?? 'Copy this location to the clipboard';
	}

	get paste(): string {
		return this._paste ?? 'Paste';
	}

	get pasteAlt(): string {
		return this._pasteAlt ?? 'Go to a point from the clipboard';
	}

	get share(): string {
		return this._share ?? 'Share';
	}

	get shareAlt(): string {
		return this._shareAlt ?? 'Copy a sharable url to the clipboard';
	}

	get center(): string {
		return this._center ?? 'Center';
	}

	get centerAlt(): string {
		return this._centerAlt ?? 'Center the map on this point';
	}

	get notifCopy(): string {
		return this._notifCopy ?? 'Copied location to clipboard';
	}

	get notifCopyFailed(): string {
		return this._notifCopyFailed ?? 'Could not copy location';
	}

	get notifPaste(): string {
		return this._notifPaste ?? 'Centered on location from clipboard';
	}

	get notifPasteFailed(): string {
		return this._notifPasteFailed ?? 'Could not paste location';
	}

	get notifPasteInvalid(): string {
		return this._notifPasteInvalid ?? 'Not a valid location';
	}

	get notifShare(): string {
		return this._notifShare ?? 'Copied shareable url to clipboard';
	}

	get notifShareFailed(): string {
		return this._notifShareFailed ?? 'Could not copy shareable url';
	}

	get notifCenter(): string {
		return this._notifCenter ?? 'Centered on location';
	}
}
