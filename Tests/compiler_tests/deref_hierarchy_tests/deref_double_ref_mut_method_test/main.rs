struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get_mut(self: &mut Self) -> i32 { self.val }
}

fn take_uniq_mut(r: &uniq &mut Foo) -> i32 {
    r.get_mut()
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    let r1: &mut Foo = &mut f;
    take_uniq_mut(&uniq r1)
}
