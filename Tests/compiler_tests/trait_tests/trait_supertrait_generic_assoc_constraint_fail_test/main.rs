trait Producer {
    let Output :! Sized;
}

trait WantsString: Producer(Output = bool) {}

impl Producer for i32 {
    let Output :! Sized = i32;
}

impl WantsString for i32 {}

fn main() -> i32 {
    5
}
