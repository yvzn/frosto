import { useCallback, useState } from "react";

import { debounce } from "./debounce";

function LocationInput(props) {
  const { value, onChange, onHomepage } = props;

  const [internalValue, setInternalValue] = useState(value);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const onChangeDebounced = useCallback(
    debounce((searchText) => onChange(searchText), 500),
    [onChange]
  );

  const onInputChange = (e) => {
    setInternalValue(e.target.value);
    onChangeDebounced(e.target.value);
  };

  return (
    <header>
      <nav>
        <button onClick={onHomepage}>Back</button>
      </nav>
      <label>
        Location:
        <input
          value={internalValue}
          onChange={onInputChange}
          placeholder="Type to search for a location"
        />
      </label>
    </header>
  );
}

export default LocationInput;
