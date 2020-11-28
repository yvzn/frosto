import Location from "./Location";
import OnboardingMessage from "./OnboardingMessage";

function Summary(props) {
  return (
    <section>
      <Location
        location={props.location}
        onLocationEdit={props.onLocationEdit}
      />
      {!props.location && <OnboardingMessage />}
    </section>
  );
}

export default Summary;
