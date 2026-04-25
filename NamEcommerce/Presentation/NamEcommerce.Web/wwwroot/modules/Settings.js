let warehouseSettings = {
    AllowNonWarehouse: false
};

export function setWarehouseSettings(settings) {
    warehouseSettings = Object.assign({}, warehouseSettings, settings);
}
export function getWarehouseSettings(settings) {
    return Object.assign({}, warehouseSettings);
}