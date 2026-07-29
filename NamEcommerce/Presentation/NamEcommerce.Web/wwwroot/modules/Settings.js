let warehouseSettings = {
    AllowNonWarehouse: false
};

export function setWarehouseSettings(settings) {
    warehouseSettings = Object.assign({}, warehouseSettings, settings);
}
export function getWarehouseSettings(settings) {
    return Object.assign({}, warehouseSettings);
}

export let customerSettings = {
    defaultCustomerId: null,
    kinds: { }
};
export function setCustomerSettings(settings) {
    customerSettings = Object.assign({}, customerSettings, settings);
}