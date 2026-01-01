import { Point } from './Point';
import { Value } from './Value';

export class Player {
	private _id: string;
	private _name: string;
	private _avatar: string;
	private _role: string;
	private _color: string;
	private _pos: Point;
	private _yaw: number;
	private _health: Value;
	private _satiety: Value;

	constructor(player?: Player) {
		this._id = player?.id ?? '';
		this._name = player?.name ?? '';
		this._avatar = player?.avatar ?? '';
		this._role = player?.role ?? '';
		this._color = player?.color ?? '';
		this._pos = player?.pos ? Point.of(player.pos) : new Point(0, 0);
		this._yaw = player?.yaw ?? 0;
		this._health = new Value(player?.health);
		this._satiety = new Value(player?.satiety);
	}

	get id(): string {
		return this._id;
	}

	get name(): string {
		return this._name;
	}

	get avatar(): string {
		return this._avatar;
	}

	get role(): string {
		return this._role;
	}

	get color(): string {
		return this._color;
	}

	get pos(): Point {
		return this._pos;
	}

	get yaw(): number {
		return this._yaw;
	}

	get health(): Value {
		return this._health;
	}

	get satiety(): Value {
		return this._satiety;
	}

	public updateData(data: Player): void {
		this._id = data.id;
		this._name = data.name;
		this._avatar = data.avatar;
		this._role = data.role;
		this._color = data.color;
		this._pos = data.pos;
		this._yaw = data.yaw;
		this._health = data.health;
		this._satiety = data.satiety;
	}
}
