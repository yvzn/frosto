import { useEffect, useState } from "react";

import Shell from "./Shell";
import locationService from "../location/location.service";

const initialState = {
  status: "INIT",
  location: undefined,
};

function App() {
  const [state, setState] = useState(initialState);

  useEffect(
    () => {
      locationService
        .getPreferredLocation()
        .then((location) => onLocationChange(location));
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  const onLocationChange = (location) => {
    setState({ ...state, status: "WEATHER", location });
  };

  const onLocationEdit = () => {
    setState({ ...state, status: "LOCATION" });
  };

  const onHomepage = () => {
    setState({ ...state, status: "WEATHER" });
  };

  return (
    <Shell
      status={state.status}
      location={state.location}
      onLocationChange={onLocationChange}
      onLocationEdit={onLocationEdit}
      onHomepage={onHomepage}
    />
  );
}

export default App;
