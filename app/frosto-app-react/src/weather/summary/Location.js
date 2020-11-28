function Location(props) {
  return (
    <header>
      <h1>{props.location?.name || "No location set"}</h1>
      <button onClick={props.onLocationEdit}>Edit</button>
    </header>
  );
}

export default Location;
