import { UserNamesPipe } from './user-names.pipe';

describe('UserNamesPipe', () => {
  it('create an instance', () => {
    const pipe = new UserNamesPipe();
    expect(pipe).toBeTruthy();
  });
});
