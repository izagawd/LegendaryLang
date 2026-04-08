struct Foo { kk: i32 }
struct Holder['a] { r: &'a Foo }

fn bad() -> Foo {
    let a = make Foo { kk: 5 };
    let dd = make Holder { r: &a };
    a
}

fn main() -> i32 { bad().kk }
