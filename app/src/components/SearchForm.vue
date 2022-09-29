<script setup>
import { ref } from "vue";
import router from "../router";

const query = ref("");

function search(event) {
	console.log(`Search for ${query.value}`);
	router.push({ path: "/search", query: { query: query.value } });
	event.preventDefault();
}

function focusOnQuery(event) {
	event.currentTarget.select();
}
</script>

<template>
	<div class="bg-light">
		<section class="container py-5">
			<form
				class="d-flex align-items-center p-3 rounded shadow-sm bg-body"
				@submit="search"
			>
				<div class="form-floating flex-grow-1">
					<input
						class="form-control"
						id="query"
						name="query"
						v-model="query"
						placeholder="Nantes, 44130"
						required="true"
						@click="focusOnQuery"
					/>
					<label for="query">Ville ou code postal</label>
				</div>
				<div class="mx-2 mx-md-3">
					<button class="btn btn-outline-primary">Rechercher</button>
				</div>
			</form>
		</section>
	</div>
</template>
