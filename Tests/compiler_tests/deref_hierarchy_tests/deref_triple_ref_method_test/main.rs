struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}

fn take_double(r: &&Foo) -> i32 {
    r.get()
}

fn main() -> i32 {
    let f = make Foo { val: 42 };
    let r1: &Foo = &f;
    take_double(&r1)
}
