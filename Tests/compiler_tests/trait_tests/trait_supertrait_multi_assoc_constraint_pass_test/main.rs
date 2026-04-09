trait Transformer {
    let Input :! type;
    let Output :! type;
}

trait I32Transformer: Transformer(Input = i32, Output = i32) {}

impl Transformer for i32 {
    let Input :! type = i32;
    let Output :! type = i32;
}

impl I32Transformer for i32 {}

fn main() -> i32 {
    42
}
