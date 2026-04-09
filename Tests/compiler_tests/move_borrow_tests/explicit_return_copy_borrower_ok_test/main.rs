struct Foo { kk: i32 }

fn good() -> Foo {
    let a = make Foo { kk: 5 };
    let r = &a;
    let val = r.kk;
    return a;
}

fn main() -> i32 { good().kk }
