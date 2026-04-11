struct Foo { val: i32 }

fn mutate_field(r: &&mut Foo) {
    r.val = 42;
}

fn main() -> i32 {
    let f = make Foo { val: 0 };
    let m = &mut f;
    mutate_field(&m);
    f.val
}
