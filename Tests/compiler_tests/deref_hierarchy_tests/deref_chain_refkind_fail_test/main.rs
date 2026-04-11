struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn need_uniq(self: &mut Self) -> i32 { self.val }
}

fn take_ref_const(r: &&const Foo) -> i32 {
    r.need_uniq()
}

fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r1: &const Foo = &const f;
    take_ref_const(&r1)
}
