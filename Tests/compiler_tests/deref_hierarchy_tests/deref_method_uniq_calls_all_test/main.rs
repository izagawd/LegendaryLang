struct Foo {
    val: i32
}
impl Copy for Foo {}

impl Foo {
    fn get_shared(self: &Self) -> i32 {
        self.val
    }
    fn get_const(self: &const Self) -> i32 {
        self.val
    }
    fn get_mut(self: &mut Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let f = make Foo { val: 14 };
    let r: &uniq Foo = &uniq f;
    r.get_shared() + r.get_const() + r.get_mut()
}
