trait Foo {
    fn get(self: &Self) -> &Self;
}

struct Bar { val: i32 }
impl Foo for Bar {
    fn get(self: &Self) -> &Self {
        self
    }
}

fn main() -> i32 {
    let b = make Bar { val: 7 };
    let r = b.get();
    r.val
}
