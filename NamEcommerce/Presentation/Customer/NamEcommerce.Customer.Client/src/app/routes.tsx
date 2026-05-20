import { useEffect, useState } from "react";

export function navigate(path: string) {
  window.history.pushState({}, "", path);
  window.dispatchEvent(new PopStateEvent("popstate"));
}

export function useRoute() {
  const [route, setRoute] = useState(() => window.location.pathname + window.location.search);

  useEffect(() => {
    const onChange = () => setRoute(window.location.pathname + window.location.search);
    window.addEventListener("popstate", onChange);
    return () => window.removeEventListener("popstate", onChange);
  }, []);

  return route;
}
