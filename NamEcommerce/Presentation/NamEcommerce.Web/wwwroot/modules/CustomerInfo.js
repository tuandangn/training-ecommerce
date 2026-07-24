let customerKinds = {};

export let customerInfo = {
    isRetailWalkInCustomer(kind) {
        return kind == customerKinds.retailWalkIn
    }
};

export function setCustomerKinds(kinds) {
    customerKinds = kinds;
}