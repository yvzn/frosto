import { defineStore } from "pinia";

export const useSearchStore = defineStore("search", {
	state: () => {
		return { query: undefined, status: "IDLE" };
	},
	actions: {
		search() {
			console.log(`Searching for ${this.query}`);
			this.status = "SEARCHING";
		},
	},
});
