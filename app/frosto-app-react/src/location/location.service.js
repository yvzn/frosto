function getPreferredLocation() {
  return Promise.resolve(undefined);
}

const locationService = {
  getPreferredLocation,
};

export default locationService;
