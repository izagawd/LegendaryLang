struct Foo { val: i32 }

fn try_mutate_field(r: &mut &Foo) {
    r.val = 42;
}

fn main() -> i32 {
    let f = make Foo { val: 0 };
    let s = &f;
    try_mutate_field(&mut s);
    f.val
}
