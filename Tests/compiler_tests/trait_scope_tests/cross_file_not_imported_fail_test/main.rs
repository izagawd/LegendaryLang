struct Foo { val: i32 }

impl crate.other.Remote for Foo {
    fn doit(self: &Self) -> i32 { self.val }
}

fn main() -> i32 {
    let f = make Foo { val: 77 };
    f.doit()
}
