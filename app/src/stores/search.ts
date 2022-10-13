import { defineStore } from "pinia";

interface SearchState {
	query: string | undefined;
	status: "IDLE" | "SEARCHING";
}

export const useSearchStore = defineStore("search", {
	state: (): SearchState => {
		return { query: undefined, status: "IDLE" };
	},
	actions: {
		search() {
			console.log(`Searching for ${this.query}`);
			this.status = "SEARCHING";
		},
	},
});
