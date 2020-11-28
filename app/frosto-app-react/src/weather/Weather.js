import Summary from "./summary/Summary";

function Weather(props) {
  return (
    <article>
      <Summary
        location={props.location}
        onLocationEdit={props.onLocationEdit}
      />
    </article>
  );
}

export default Weather;
