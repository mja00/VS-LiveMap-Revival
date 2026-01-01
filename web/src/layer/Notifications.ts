import * as L from 'leaflet';

export class Notifications {
	private readonly _dom: HTMLElement;

	constructor() {
		this._dom = L.DomUtil.create('div', 'notifications');
		document.body.append(this._dom);
	}

	public create(type: ('info' | 'success' | 'warning' | 'danger'), text: string): void {
		const div: HTMLElement = this._dom.appendChild(L.DomUtil.create('div', type));
		div.append(window.createSVGIcon(type));
		div.appendChild(L.DomUtil.create('p')).innerText = text;

		const handler = (): void => {
			div.removeEventListener('transitionend', handler);
			setTimeout((): void => {
				div.addEventListener('transitionend', (): void => {
					div.remove();
				}, { passive: true });
				div.classList.remove('show');
			}, 2500);
		};
		div.addEventListener('transitionend', handler, { passive: true });
		setTimeout((): void => div.classList.add('show'), 50);
	}

	public info(text: string): void {
		this.create('info', text);
	}

	public success(text: string): void {
		this.create('success', text);
	}

	public warning(text: string): void {
		this.create('warning', text);
	}

	public danger(text: string): void {
		this.create('danger', text);
	}
}
