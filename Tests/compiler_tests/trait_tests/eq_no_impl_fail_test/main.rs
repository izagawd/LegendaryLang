struct Foo { val: i32 }

fn main() -> i32 {
    let a = make Foo { val: 1 };
    let b = make Foo { val: 1 };
    if a == b { 1 } else { 0 }
}
