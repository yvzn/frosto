import { useCallback } from "react";
import LocationInput from "./LocationInput";

function LocationPicker(props) {
  const { location, onLocationChange, onHomepage } = props;

  const onInputChange = useCallback(
    (searchText) =>
      onLocationChange({
        name: searchText,
        latitude: 47.475279,
        longitude: -1.764935,
      }),
    [onLocationChange]
  );

  return (
    <article>
      <LocationInput
        value={location?.name}
        onChange={onInputChange}
        onHomepage={onHomepage}
      />
    </article>
  );
}

export default LocationPicker;
