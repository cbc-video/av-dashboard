import { AvPage } from './app.po';

describe('av App', function() {
  let page: AvPage;

  beforeEach(() => {
    page = new AvPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
