import type { CustomerLocation } from "../api/types";

export function getCurrentCustomerLocation(): Promise<CustomerLocation | null> {
  if (!("geolocation" in navigator)) {
    return Promise.resolve(null);
  }

  return new Promise((resolve) => {
    navigator.geolocation.getCurrentPosition(
      (position) => {
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracyMeters: position.coords.accuracy,
          capturedOnUtc: new Date().toISOString(),
        });
      },
      () => resolve(null),
      {
        enableHighAccuracy: false,
        timeout: 3000,
        maximumAge: 300000,
      },
    );
  });
}
