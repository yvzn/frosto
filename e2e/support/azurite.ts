// Reads the local Azurite tables (the stack runs with UseDevelopmentStorage=true).

import { TableClient } from '@azure/data-tables';

const table = (name: string) =>
	TableClient.fromConnectionString('UseDevelopmentStorage=true', name, { allowInsecureConnection: true });

/** Slot and day where admin's "Create Batches" put a valid location (locationbatch table, same keys). */
export async function batchSlotOf(partitionKey: string, rowKey: string): Promise<{ slotIndex: number; dayIndex: number }> {
	const entity = await table('locationbatch').getEntity<{ slot_index: number; day_index: number }>(partitionKey, rowKey);
	return { slotIndex: entity.slot_index, dayIndex: entity.day_index };
}
