trait Foo: Sized {
    fn make() -> Self;
}

struct Bar { val: i32 }
impl Foo for Bar {
    fn make() -> Self {
        make Bar { val: 99 }
    }
}

fn main() -> i32 {
    let b = (Bar as Foo).make();
    b.val
}
