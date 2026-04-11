struct Foo {
    val: i32
}
impl Copy for Foo {}

impl Foo {
    fn get_shared(self: &Self) -> i32 {
        self.val
    }
    fn get_const_CONST_REFERENCE_TYPES_ARE_NOW_DEPRECATED(self: & Self) -> i32 {
        self.val
    }
    fn get_mut(self: &mut Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let f = make Foo { val: 14 };
    let r: &mut Foo = &mut f;
    r.get_shared() + r.get_const() + r.get_mut()
}
