struct Foo { val: i32 }

impl Foo {
    fn get(self: &Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let foo = make Foo { val: 5 };
    foo.get() + foo.get()
}
