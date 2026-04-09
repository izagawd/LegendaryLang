use crate.other.Remote;

trait Local {
    fn doit(self: &Self) -> i32;
}

struct Foo { val: i32 }

impl Local for Foo {
    fn doit(self: &Self) -> i32 { 1 }
}

impl Remote for Foo {
    fn doit(self: &Self) -> i32 { 2 }
}

fn main() -> i32 {
    let f = make Foo { val: 0 };
    f.doit()
}
