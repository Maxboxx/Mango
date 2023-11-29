
/**
 * Bit flags for mango data.
 */
export enum MangoFlags {
	/** No flags. */
	none = 0,

	/** Flag for pretty output. */
	pretty = 1,

	/** Flag for always separating values with commas. */
	commas = 2,

	/** Flag for always surrounding keys with quotes. */
	quotes = 4,

	/** Flag for using the null keyword instead of nil. */
	useNull = 8,
	
	/** Flags for json output. */
	json = commas | quotes | useNull
}

/**
 * Static class for parsing mango data.
 */
export class Mango {
	/**
	 * Parses mango data.
	 * @param str The mango data string to parse.
	 * @param vars An object containing variables to include in the mango data.
	 * @returns The parsed mango data.
	 */
	static parse(str: string, vars?: object): any;
	
	/**
	 * Converts mango data to a string.
	 * @param data The mango data to convert to a string.
	 * @param flags Bit flags for the conversion.
	 * @returns A string containing mango data.
	 */
	static toString(data: any, flags?: MangoFlags): string;
}
