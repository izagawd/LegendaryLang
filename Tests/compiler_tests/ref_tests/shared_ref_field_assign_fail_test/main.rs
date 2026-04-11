struct Foo { val: i32 }

fn shared_write(f: &Foo) {
    f.val = 99;
}

fn main() -> i32 {
    let f = make Foo { val: 0 };
    shared_write(&f);
    f.val
}
