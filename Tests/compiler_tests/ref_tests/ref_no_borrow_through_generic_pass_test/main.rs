struct Plain { val: i32 }

fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let p = make Plain { val: 42 };
    let p2 = PassAround(p);
    p2.val
}
