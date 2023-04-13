
/// Mango stuff.
class Mango {
	/// No flags.
	static none = 0;
	
	/// Flag for pretty output.
	static pretty = 1;
	
	/// Flag for using commas as separators.
	static commas = 2;
	
	/// Flag for surrounding all keys with quotes. 
	static quotes = 4;
	
	/// Flag for using the null keyword instead of nil.
	static useNull = 8;
	
	/// Flag for json output.
	static json = 2 | 4 | 8;
	
	/// Parses a mango string.
	static parse(str, vars = {}) {
		const parser = new Mango.#Parser(str);
		
		for (let v in vars) {
			parser.scope.vars[v] = new Mango.#Value(vars[v]);
		}
		
		const value = parser.parseTaggedValue();
		parser.parseWhitespace();
		
		if (!parser.isDone) {
			throw 'Invalid mango';
		}
		
		return value.evaluate(parser.scope);
	}
	
	/// Converts mango data to a string.
	static toString(data, flags = Mango.none) {
		const writer = new Mango.#Writer(flags);
		writer.writeValue(data);
		return writer.toString();
	}

	static #Value = class {
		constructor(value) {
			this.value = value;
		}
		
		evaluate(data) {
			return this.value;
		}
	};

	static #Var = class {
		constructor(name) {
			this.name  = name;
			this.isVar = true;
		}
		
		evaluate(data) {
			return data.vars[this.name]?.evaluate(data) ?? null;
		}
		
		value(data) {
			return data.vars[this.name] ?? new Mango.#Value(null);
		}
	};

	static #Unpack = class {
		constructor(value) {
			this.value = value;
			this.isUnpack = true;
		}
		
		evaluate(data, parent) {
			const value = this.value.evaluate(data) ?? null;
			
			if (Array.isArray(value) && Array.isArray(parent)) {
				for (let v of value) {
					parent.push(v);
				}
			}
			else if (value !== null && parent !== null && typeof(value) == 'object' && typeof(parent) == 'object') {
				for (let key in value) {
					parent[key] = value[key];
				}
			}
			
			return parent; 
		}
	};

	static #TemplateDef = class {
		constructor(data, args, value) {
			this.data  = data;
			this.args  = args;
			this.value = value;
		}
		
		evaluate(args) {
			let scope = this.data.copy();
			
			for (let i = 0; i < this.args.length; i++) {
				if (i < args.length) {
					scope.vars[this.args[i]] = args[i];
				}
				else {
					scope.vars[this.args[i]] = new Mango.#Value(null);
				}
			}
			
			return this.value.evaluate(scope);
		}
	};

	static #Template = class {
		constructor(name, args) {
			this.name = name;
			this.args = args;
		}
		
		evaluate(data) {
			let args = [];
			
			for (let i = 0; i < this.args.length; i++) {
				if (this.args[i].isVar) {
					args.push(this.args[i].value(data));
				}
				else {
					args.push(this.args[i]);
				}
			}
			
			return data.templates[this.name]?.evaluate(args) ?? null;
		}
	};

	static #String = class {
		constructor(segments) {
			this.segments = segments ?? [];
		}
		
		evaluate(data) {
			let str = '';
			
			for (let seg of this.segments) {
				let v = seg.evaluate(data);
				
				if (typeof(v) == 'string') {
					str += v;
				}
				else {
					str += Mango.toString(seg.evaluate(data));
				}
			}
			
			return str;
		}
	};

	static #List = class {
		constructor(data, list) {
			this.data = data;
			this.list = list ?? [];
		}
		
		evaluate(data) {
			let list = [];
			
			let scope = this.data.combine(data);
			
			for (let v of this.list) {
				if (v.isUnpack) {
					v.evaluate(scope, list);
				}
				else {
					list.push(v.evaluate(scope, list));
				}
			}
			
			return list;
		}
	};

	static #Obj = class {
		constructor(data, obj, unpack) {
			this.data = data;
			this.obj = obj ?? {};
			this.unpack = unpack ?? [];
		}
		
		evaluate(data) {
			let obj = {};
			
			let scope = this.data.combine(data);
			
			for (let u of this.unpack) {
				u.evaluate(scope, obj);
			}
			
			for (let key in this.obj) {
				obj[key] = this.obj[key].evaluate(scope);
			}
			
			return obj;
		}
	};

	static #Scope = class {
		constructor() {
			this.vars = {};
			this.templates = {};
		}
		
		copy() {
			let scope = new Mango.#Scope();
			
			for (let v in this.vars) {
				scope.vars[v] = this.vars[v];
			}
			
			for (let v in this.templates) {
				scope.templates[v] = this.templates[v];
			}
			
			return scope;
		}
		
		combine(parent) {
			let scope = parent.copy();
			
			for (let v in this.vars) {
				scope.vars[v] = this.vars[v];
			}
			
			for (let v in this.templates) {
				scope.templates[v] = this.templates[v];
			}
			
			return scope;
		}
	};

	static #Parser = class {
		constructor(str) {
			this.str = str;
			this.index = 0;
			this.scope = new Mango.#Scope();
			this.parseWhitespace();
		}
		
		get current() {
			return this.str.charAt(this.index);
		}
		
		get isDone() {
			return this.index >= this.str.length;
		}
		
		peek(text) {
			for (let i = 0; i < text.length; i++) {
				if (this.str.charAt(this.index + i) !== text.charAt(i)) return false;
			} 
			
			return true;
		}
		
		parseRaw(text) {
			if (this.peek(text)) {
				this.index += text.length;
				return true;
			}
			
			return false;
		}
		
		parseText(text) {
			if (this.parseRaw(text)) {
				this.parseWhitespace();
				return true;
			}
			
			return false;
		}
		
		parseToken(pattern) {
			let token = this.parseRegex(pattern);
			
			if (token) {
				this.parseWhitespace();
				return token;
			}
		}
		
		parseRegex(pattern) {
			pattern.lastIndex = this.index;
			const match = pattern.exec(this.str);
			
			if (match) {
				this.index += match[0].length;
				return match.length > 1 ? match[1] : match[0];
			}
		}
		
		parseWhitespace() {
			this.parseRegex(/[\s\r\n]+/y);
			
			let start = this.parseRegex(/\-\-(\/*)/y);
			if (start == undefined) return;
			
			let block = start.length;
			
			if (block == 0) {
				while (this.current != '\r' && this.current != '\n' && !this.isDone) {
					this.index++;
				}
			}
			else {
				while (true) {
					while (this.current != '/') {
						this.index++;
						
						if (this.isDone) throw 'Invalid comment';
					}
					
					let count = 0;
					
					while (this.current == '/') {
						count++;
						this.index++;
					}
					
					if (count >= block && this.parseRaw('--')) {
						break;
					}
					
					this.index++;
				}
			}
			
			this.parseRegex(/[\s\r\n]+/y);
		}
		
		parseName() {
			let i = this.index;
			let name = this.parseToken(/[a-zA-Z0-9_]+\b/y);
			
			if (!name || name == 'true' || name == 'false' || name == 'null' || name == 'nil' || /^[0-9]+$/.test(name)) {
				this.index = i;
				return;
			}
			
			return name;
		}
		
		parseTaggedValue() {
			let tags = [];
			
			const index = this.index;
			
			while (!this.isDone) {
				let tag = this.parseKey();
				if (tag == undefined) break;
				tags.push(tag);
			}
			
			if (tags.length > 0) {
				if (!this.parseText(':')) {
					this.index = index;
					return this.parseValue();
				}
			}
			
			return this.parseValue();
		}
		
		/// Parses a value.
		parseValue() {
			switch (this.current) {
				case 'n': {
					if (this.parseToken(/(nil|null)\b/y)) return new Mango.#Value(null);
				}
				
				case 't': 
				case 'f': {
					let t = this.parseToken(/(true|false)\b/y);
					if (t) return new Mango.#Value(t == 'true');
					break;
				}
				
				case '"': {
					return this.parseString();
				}
				
				case '[': {
					return this.parseList();
				}
				
				case '{': {
					return this.parseObject();
				}
				
				case '$': {
					let v = this.parseVariable();
					return new Mango.#Var(v);
				}
				
				case '#': {
					return this.parseTemplateValue();
				}
			}
			
			let t = this.parseToken(/\d*\.?\d+\b/y);
			if (t) return new Mango.#Value(parseFloat(t));
			
			throw 'Invalid mango value';
		}
		
		parseRawString() {
			return this.parseString(true).value;
		}
		
		parseString(simple = false) {
			if (!this.parseRaw('"')) throw 'Invalid string';
			
			const start = this.index;
			let str = '';
			let segments = [];
			
			while (!this.isDone) {
				switch (this.current) {
					case '"': {
						this.index++;
						this.parseWhitespace();
						
						if (!simple && segments.length > 0) {
							segments.push(new Mango.#Value(str));
							return new Mango.#String(segments);
						}
						
						return new Mango.#Value(str);
					}
					
					case '\r':
					case '\n': {
						throw 'Invalid string';
					}
					
					case '\\': {
						switch (this.str.charAt(this.index + 1)) {
							case 't': str += '\t'; break;
							case 'r': str += '\r'; break;
							case 'n': str += '\n'; break;
							
							case '{': {
								if (simple) break;
								this.index += 2;
								this.parseWhitespace();
								segments.push(new Mango.#Value(str));
								str = '';
								segments.push(this.parseTaggedValue());
								
								if (!this.parseRaw('}')) throw 'Invalid string';
								this.index -= 2;
								break;
							}
							
							default: {
								str += this.str.charAt(this.index + 1);
							}
						}
						
						this.index += 2;
						continue;
					}
				}
				
				str += this.current;
				this.index++;
			}
			
			throw 'Invalid string';
		}
		
		/// Parses a list.
		parseList() {
			if (!this.parseText('[')) throw 'Invalid list';
			
			let array = [];
			let scope = this.scope.copy();
			
			while (true) {
				if (this.parseVarAssign()) continue;
				if (this.parseTemplateAssign()) continue;
				break;
			}
			
			while (!this.isDone) {
				let v = this.parseUnpack();
				
				if (v !== undefined) {
					array.push(v);
				}
				else {
					array.push(this.parseTaggedValue());
				}
				
				if (this.parseText(']')) {
					const s = this.scope.copy();
					this.scope = scope;
					return new Mango.#List(s, array);
				}
				
				this.parseText(',');
			}
			
			throw 'Invalid list';
		}
		
		parseObject() {
			if (!this.parseText('{')) throw 'Invalid object';
			
			let obj  = {};
			let unpack = [];
			let scope = this.scope.copy();
			
			while (true) {
				if (this.parseVarAssign()) continue;
				if (this.parseTemplateAssign()) continue;
				break;
			}
			
			if (this.parseText('}')) {
				const s = this.scope.copy();
				this.scope = scope;
				return new Mango.#Obj(s, obj, unpack);
			}
			
			while (!this.isDone) {
				let v = this.parseUnpack();
				
				if (v !== undefined) {
					unpack.push(v);
				}
				else {
					let key = this.parseKey();
					
					if (key == undefined) throw 'Invalid object';
				
					let tags = [];
					
					while (!this.isDone) {
						let tag = this.parseKey();
						if (tag == undefined) break;
						
						tags.push(tag);
					}
					
					if (!this.parseText(':')) throw 'Invalid object';
					
					let val = this.parseValue();
				
					obj[key] = val;
				}
				
				if (this.parseText('}')) {
					const s = this.scope.copy();
					this.scope = scope;
					return new Mango.#Obj(s, obj, unpack);
				}
				
				this.parseText(',');
			}
			
			throw 'Invalid object';
		}
		
		/// Parses a key.
		parseKey() {
			if (this.current == '"') return this.parseRawString();
			return this.parseName();
		}
		
		parseUnpack() {
			let u = this.parseVarUnpack();
			if (u) return u;
			
			u = this.parseTemplateUnpack();
			if (u) return u;
		}
		
		/// Parses a variable.
		parseVariable() {
			return this.parseToken(/\$([a-zA-Z0-9_]+)\b/y);
		}
		
		/// Parses a variable unpack.
		parseVarUnpack() {
			let v = this.parseToken(/\$\$([a-zA-Z0-9_]+)\b/y);
			if (v == undefined) return;
			return new Mango.#Unpack(new Mango.#Var(v));
		}
		
		/// Parses a variable assignment.
		parseVarAssign() {
			const index = this.index;
			
			if (this.peek('$$')) return;
			
			let v = this.parseVariable();
			
			if (!this.parseText('=')) {
				this.index = index;
				return;
			}
			
			let val = this.parseValue();
			
			this.scope.vars[v] = val;
			
			return v;
		}
		
		/// Parses a template name.
		parseTemplateName() {
			return this.parseToken(/\#([a-zA-Z0-9_]+)\b/y);
		}
		
		/// Parses a template.
		parseTemplateValue() {
			let name = this.parseTemplateName();
			
			if (!name) throw 'Invalid template';
			
			let args = [];
			
			if (!this.parseText('[')) throw 'Invalid template';		
			
			if (this.parseText(']')) {
				return new Mango.#Template(name, args);
			}
			
			while (!this.isDone) {
				let v = this.parseValue();
				
				if (v) {
					args.push(v);
				}
				
				if (this.parseText(']')) {
					return new Mango.#Template(name, args);
				}
				
				this.parseText(',');
			}
			
			throw 'Invalid template';
		}
		
		/// Parses a template unpack.
		parseTemplateUnpack() {
			if (!this.peek('##')) return;
			this.index++;
			let v = this.parseTemplateValue();
			return new Mango.#Unpack(v);
		}
		
		/// Parses a template assignment.
		parseTemplateAssign() {
			const index = this.index;
			
			if (this.peek('##')) return;
			
			let name = this.parseTemplateName();
			let args = [];
			let scope = this.scope.copy();
			
			if (!this.parseText('[')) {
				return;
			}
			
			if (!this.parseText(']')) {
				while (!this.isDone) {
					let v = this.parseVariable();
					if (v == undefined) {
						this.index = index;
						return;
					}
					
					args.push(v);
					
					if (this.parseText(']')) {
						break;
					}
					
					this.parseText(',');
				}
			}
			
			if (!this.parseText('=')) {
				this.index = index;
				return;
			}
			
			let val = this.parseValue();
			
			this.scope = scope;
			this.scope.templates[name] = new Mango.#TemplateDef(this.scope.copy(), args, val);
			
			return name;
		}
	};

	static #Writer = class {
		constructor(flags) {
			this.flags = flags;
			this.segments = [];
		}
		
		toString() {
			return this.segments.join('');
		}
		
		writeValue(value, t = '') {
			if (value == null) {
				this.segments.push(this.flags & Mango.useNull ? 'null' : 'nil');
			}
			else if (value === true) {
				this.segments.push('true');
			}
			else if (value === false) {
				this.segments.push('false');
			}
			else if (Array.isArray(value)) {
				this.writeList(value, t);
			}
			else if (typeof(value) == 'object') {
				this.writeObject(value, t);
			}
			else {
				this.segments.push(JSON.stringify(value));
			}
		}
		
		writeObject(obj, t) {
			const pretty = this.flags & Mango.pretty;
			const nt = t + '\t';
			
			if (pretty) {
				this.segments.push('{\n' + nt);
			}
			else {
				this.segments.push('{');
			}
			
			const keys = Object.keys(obj);
			
			for (let i = 0; i < keys.length; i++) {
				if (i > 0) {
					if (this.flags & Mango.commas) {
						this.segments.push(pretty ? ',\n' + nt : ',');
					}
					else {
						this.segments.push(pretty ? '\n' + nt : ' ');
					}
				}
				
				if (this.flags & Mango.quotes || !((/^[a-zA-Z0-9_]+$/).test(keys[i]) && (/[a-zA-Z_]/).test(keys[i]))) {
					this.segments.push(JSON.stringify(keys[i]));
				}
				else {
					this.segments.push(keys[i]);
				}
				
				this.segments.push(pretty ? ': ' : ':');
				this.writeValue(obj[keys[i]], nt);
			}
			
			if (pretty) {
				this.segments.push('\n' + t + '}');
			}
			else {
				this.segments.push('}');
			}
		}
		
		writeList(list, t) {
			const nt = t + '\t';
			const pretty = this.flags & Mango.pretty;
			
			if (pretty) {
				this.segments.push('[\n' + nt);
			}
			else {
				this.segments.push('[');
			}
			
			for (let i = 0; i < list.length; i++) {
				if (i > 0) {
					if (this.flags & Mango.commas) {
						this.segments.push(pretty ? ',\n' + nt : ',');
					}
					else {
						this.segments.push(pretty ? '\n' + nt : ' ');
					}
				}
				
				this.writeValue(list[i], nt);
			}
			
			if (pretty) {
				this.segments.push('\n' + t + ']');
			}
			else {
				this.segments.push(']');
			}
		}
	};
}
